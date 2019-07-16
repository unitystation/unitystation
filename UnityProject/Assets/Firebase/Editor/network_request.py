# Copyright 2019 Google Inc. All Rights Reserved.
"""Wrapper script which makes a network request.

Basic Usage: network_request.py post
                                --url <url>
                                --header <header>  (optional, support multiple)
                                --body <body>       (optional)
                                --timeout <secs>    (optional)
                                --verbose           (optional)
"""

import argparse
import httplib
import inspect
import logging
import socket
import sys
import urlparse

# Set up logger as soon as possible
formatter = logging.Formatter('[%(levelname)s] %(message)s')

handler = logging.StreamHandler(stream=sys.stdout)
handler.setFormatter(formatter)
handler.setLevel(logging.INFO)

logger = logging.getLogger(__name__)
logger.addHandler(handler)
logger.setLevel(logging.INFO)

# Custom exit codes for known issues.
# System exit codes in python are valid from 0 - 256, so we will map some common
# ones here to understand successes and failures.
# Uses lower ints to not collide w/ HTTP status codes that the script may return
EXIT_CODE_SUCCESS = 0
EXIT_CODE_SYS_ERROR = 1
EXIT_CODE_INVALID_REQUEST_VALUES = 2
EXIT_CODE_GENERIC_HTTPLIB_ERROR = 3
EXIT_CODE_HTTP_TIMEOUT = 4
EXIT_CODE_HTTP_REDIRECT_ERROR = 5
EXIT_CODE_HTTP_NOT_FOUND_ERROR = 6
EXIT_CODE_HTTP_SERVER_ERROR = 7
EXIT_CODE_HTTP_UNKNOWN_ERROR = 8

MAX_EXIT_CODE = 8

# All used http verbs
POST = 'POST'


def unwrap_kwarg_namespace(func):
  """Transform a Namespace object from argparse into proper args and kwargs.

  For a function that will be delegated to from argparse, inspect all of the
  argments and extract them from the Namespace object.

  Args:
    func: the function that we are wrapping to modify behavior

  Returns:
    a new function that unwraps all of the arguments in a namespace and then
    delegates to the passed function with those args.
  """
  # When we move to python 3, getfullargspec so that we can tell the
  # difference between args and kwargs -- then this could be used for functions
  # that have both args and kwargs
  argspec = inspect.getargspec(func)

  def wrapped(argparse_namespace=None, **kwargs):
    """Take a Namespace object and map it to kwargs.

    Inspect the argspec of the passed function. Loop over all the args that
    are present in the function and try to map them by name to arguments in the
    namespace. For keyword arguments, we do not require that they be present
    in the Namespace.

    Args:
      argparse_namespace: an arparse.Namespace object, the result of calling
        argparse.ArgumentParser().parse_args()
      **kwargs: keyword arguments that may be passed to the original function
    Returns:
      The return of the wrapped function from the parent.

    Raises:
      ValueError in the event that an argument is passed to the cli that is not
      in the set of named kwargs
    """
    if not argparse_namespace:
      return func(**kwargs)

    reserved_namespace_keywords = ['func']
    new_kwargs = {}

    args = argspec.args or []
    for arg_name in args:
      passed_value = getattr(argparse_namespace, arg_name, None)
      if passed_value is not None:
        new_kwargs[arg_name] = passed_value

    for namespace_key in vars(argparse_namespace).keys():
      # ignore namespace keywords that have been set not passed in via cli
      if namespace_key in reserved_namespace_keywords:
        continue

      # make sure that we haven't passed something we should be processing
      if namespace_key not in args:
        raise ValueError('CLI argument "{}" does not match any argument in '
                         'function {}'.format(namespace_key, func.__name__))

    return func(**new_kwargs)

  wrapped.__name__ = func.__name__
  return wrapped


class NetworkRequest(object):
  """A container for an network request object.

  This class holds on to all of the attributes necessary for making a
  network request via httplib.
  """

  def __init__(self, url, method, headers, body, timeout):
    self.url = url.lower()
    self.parsed_url = urlparse.urlparse(self.url)
    self.method = method
    self.headers = headers
    self.body = body
    self.timeout = timeout
    self.is_secure_connection = self.is_secure_connection()

  def execute_request(self):
    """"Execute the request, and get a response.

    Returns:
      an HttpResponse object from httplib
    """
    if self.is_secure_connection:
      conn = httplib.HTTPSConnection(self.get_hostname(), timeout=self.timeout)
    else:
      conn = httplib.HTTPConnection(self.get_hostname(), timeout=self.timeout)

    conn.request(self.method, self.url, self.body, self.headers)
    response = conn.getresponse()
    return response

  def get_hostname(self):
    """Return the hostname for the url."""
    return self.parsed_url.netloc

  def is_secure_connection(self):
    """Checks for a secure connection of https.

    Returns:
      True if the scheme is "https"; False if "http"

    Raises:
      ValueError when the scheme does not match http or https
    """
    scheme = self.parsed_url.scheme

    if scheme == 'http':
      return False
    elif scheme == 'https':
      return True
    else:
      raise ValueError('The url scheme is not "http" nor "https"'
                       ': {}'.format(scheme))


def parse_colon_delimited_options(option_args):
  """Parses a key value from a string.

  Args:
      option_args: Key value string delimited by a color, ex: ("key:value")

  Returns:
      Return an array with the key as the first element and value as the second

  Raises:
      ValueError: If the key value option is not formatted correctly
  """
  options = {}

  if not option_args:
    return options

  for single_arg in option_args:
    values = single_arg.split(':')
    if len(values) != 2:
      raise ValueError('An option arg must be a single key/value pair '
                       'delimited by a colon - ex: "thing_key:thing_value"')

    key = values[0].strip()
    value = values[1].strip()
    options[key] = value

  return options


def make_request(request):
  """Makes a synchronous network request and return the HTTP status code.

  Args:
    request: a well formulated request object

  Returns:
    The HTTP status code of the network request.
    '1' maps to invalid request headers.
  """

  logger.info('Sending network request -')
  logger.info('\tUrl: %s', request.url)
  logger.debug('\tMethod: %s', request.method)
  logger.debug('\tHeaders: %s', request.headers)
  logger.debug('\tBody: %s', request.body)

  try:
    response = request.execute_request()
  except socket.timeout:
    logger.exception(
        'Timed out post request to %s in %d seconds for request body: %s',
        request.url, request.timeout, request.body)
    return EXIT_CODE_HTTP_TIMEOUT
  except (httplib.HTTPException, socket.error):
    logger.exception(
        'Encountered generic exception in posting to %s with request body %s',
        request.url, request.body)
    return EXIT_CODE_GENERIC_HTTPLIB_ERROR

  status = response.status
  headers = response.getheaders()
  logger.info('Received Network response -')
  logger.info('\tStatus code: %d', status)
  logger.debug('\tResponse headers: %s', headers)

  if status < 200 or status > 299:
    logger.error('Request (%s) failed with status code %d\n', request.url,
                 status)

  # If we wanted this script to support get, we need to
  # figure out what mechanism we intend for capturing the response
  return status


@unwrap_kwarg_namespace
def post(url=None, header=None, body=None, timeout=5, verbose=False):
  """Sends a post request.

  Args:
      url: The url of the request
      header: A list of headers for the request
      body: The body for the request
      timeout: Timeout in seconds for the request
      verbose: Should debug logs be displayed

  Returns:
      Return an array with the key as the first element and value as the second
  """

  if verbose:
    handler.setLevel(logging.DEBUG)
    logger.setLevel(logging.DEBUG)

  try:
    logger.info('Parsing headers: %s', header)
    headers = parse_colon_delimited_options(header)
  except ValueError:
    logging.exception('Could not parse the parameters with "--header": %s',
                      header)
    return EXIT_CODE_INVALID_REQUEST_VALUES

  try:
    request = NetworkRequest(url, POST, headers, body, float(timeout))
  except ValueError:
    logger.exception('Invalid request values passed into the script.')
    return EXIT_CODE_INVALID_REQUEST_VALUES

  status = make_request(request)

  # View exit code after running to get the http status code: 'echo $?'
  return status


def get_argsparser():
  """Returns the argument parser.

  Returns:
    Argument parser for the script.
  """

  parser = argparse.ArgumentParser(
      description='The script takes in the arguments of a network request. '
      'The network request is sent and the http status code will be'
      'returned as the exit code.')
  subparsers = parser.add_subparsers(help='Commands:')
  post_parser = subparsers.add_parser(
      post.__name__, help='{} help'.format(post.__name__))
  post_parser.add_argument(
      '--url',
      help='Request url. Ex: https://www.google.com/somePath/',
      required=True,
      dest='url')
  post_parser.add_argument(
      '--header',
      help='Request headers as a space delimited list of key '
      'value pairs. Ex: "key1:value1 key2:value2"',
      action='append',
      required=False,
      dest='header')
  post_parser.add_argument(
      '--body',
      help='The body of the network request',
      required=True,
      dest='body')
  post_parser.add_argument(
      '--timeout',
      help='The timeout in seconds',
      default=10.0,
      required=False,
      dest='timeout')
  post_parser.add_argument(
      '--verbose',
      help='Should verbose logging be outputted',
      action='store_true',
      default=False,
      required=False,
      dest='verbose')
  post_parser.set_defaults(func=post)
  return parser


def map_http_status_to_exit_code(status_code):
  """Map an http status code to the appropriate exit code.

  Exit codes in python are valid from 0-256, so we want to map these to
  predictable exit codes within range.

  Args:
    status_code: the input status code that was output from the network call
                 function

  Returns:
    One of our valid exit codes declared at the top of the file or a generic
    unknown error code
  """
  if status_code <= MAX_EXIT_CODE:
    return status_code

  if status_code > 199 and status_code < 300:
    return EXIT_CODE_SUCCESS

  if status_code == 302:
    return EXIT_CODE_HTTP_REDIRECT_ERROR

  if status_code == 404:
    return EXIT_CODE_HTTP_NOT_FOUND_ERROR

  if status_code > 499:
    return EXIT_CODE_HTTP_SERVER_ERROR

  return EXIT_CODE_HTTP_UNKNOWN_ERROR


def main():
  """Main function to run the program.

  Parse system arguments and delegate to the appropriate function.

  Returns:
    A status code - either an http status code or a custom error code
  """
  parser = get_argsparser()
  subparser_action = parser.parse_args()
  try:
    return subparser_action.func(subparser_action)
  except ValueError:
    logger.exception('Invalid arguments passed.')
    parser.print_help(sys.stderr)
    return EXIT_CODE_INVALID_REQUEST_VALUES
  return EXIT_CODE_GENERIC_HTTPLIB_ERROR

if __name__ == '__main__':
  exit_code = map_http_status_to_exit_code(main())
  sys.exit(exit_code)
